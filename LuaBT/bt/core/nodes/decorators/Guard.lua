
-----------------------------------------------------------------------------------------
-- Decorators
-- Guard

-- Protect the decorated child from running if another Guard with the same token is 
-- already guarding (Running) that token.\n
-- Guarding is global for all of the agent's Behaviour Trees.
-----------------------------------------------------------------------------------------

local Guard = bt.Class("Guard",bt.BTDecorator)
bt.Guard = Guard

GuardMode =
{
    ReturnFailure       = 0,
    WaitUntilReleased   = 1,
}
        
function Guard:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "Guard"
    self.token = nil
    self.ifGuarded = GuardMode.ReturnFailure
    self.isGuarding = false
    print("ERROR:Guard not support yet")
end

function Guard:init(jsonData)
    if jsonData.token ~= nil then
        self.token = jsonData.token._value
    end
    if jsonData.ifGuarded ~= nil then
        self.ifGuarded = GuardMode[jsonData.ifGuarded]
    end
end

function Guard:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Failure
    end
    self:setGuard(agent)
    for k,guard in pairs(bt.guards[agent]) do
        if guard ~= self and guard.isGuarding and guard.token == self.token then
            if self.ifGuarded == GuardMode.ReturnFailure then
                return bt.Status.Failure
            else
                return bt.Status.Running
            end
        end
    end
    self.status = decoratedConnection:execute(agent,blackboard)
    if self.status == bt.Status.Running then
        self.isGuarding = true
        return bt.Status.Running
    end
    self.isGuarding = false
    return self.status
end

function Guard:setGuard(agent)
    if bt.guards[agent] == nil then
        bt.guards[agent] = {}
    end
    if not self:isContainInGuards(agent,self) and 
       self.token ~= nil and 
       string.len(self.token) > 0 then
        table.insert(bt.guards[agent],self)
    end
end

function Guard:isContainInGuards(agent,guard)
    if bt.guards[agent] then
        for k,v in pairs(bt.guards[agent]) do
            if v == guard then 
                return true
            end
        end
    end
    return false
end

function Guard:onGraphStarted()
end

function Guard:onReset()
    self.isGuarding = false
end

function Guard:destroy()
end
