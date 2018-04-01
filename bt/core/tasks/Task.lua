
local Task = bt.Class("Task")
bt.Task = Task

function Task:ctor()
    self.name = "Task"
    self.isActive = true
    self.agent = nil
    self.blackboard = nil
end

function Task:set(agent,blackboard)
    self.blackboard = blackboard
    if self.agent ~= nil and agent ~= nil and self.agent == agent then
        self.isActive = true
        return self.isActive
    end
    self.isActive = self.initialize(agent)
    return self.isActive
end

function Task:initialize(agent)
    self.agent = agent
    local error = onInit()
    if error ~= nil then
        print("Task initialize ERROR:"..error)
        return false
    end
    return true
end

function Task:info()
    return self.name
end

function Task:onCreate()
end

function Task:onValidate()
end

function Task:onInit()
    return nil
end