local BehaviourTree = bt.Class("BehaviourTree")
bt.BehaviourTree = BehaviourTree

function BehaviourTree:ctor()
    self.id = 0
    self.name = "BehaviourTree"
    self.primeNode = nil
    self.nodes = {}
    self.rootStatus = bt.Status.Resting
    self.agent = nil
    self.blackboard = nil
    self.isRunning = false
    self.isPaused = false
    self.isRepeat = true
end

function BehaviourTree:start()
    self.rootStatus = self.primeNode.status
end

function BehaviourTree:update()
    if self:tick(self.agent,self.blackboard) ~= bt.Status.Running and 
        not self.isRepeat then
        self.stop(self.rootStatus == bt.Status.Success)
    end
end

function BehaviourTree:tick(agent,blackboard)
    if self.rootStatus ~= bt.Status.Running then
        self.primeNode:reset()
    end
    self.rootStatus = primeNode:execute(agent,blackboard)
    return self.rootStatus
end

function BehaviourTree:stop(success)
    if not self.isRunning and not self.isPaused then
        return
    end
    self.isRunning = false
    self.isPaused = false
    for k,node in pairs(self.nodes) do
        node:reset(false)
        node:onGraphStoped()
    end
end

function BehaviourTree:pause()
    if not self.isRunning then
        return
    end
    self.isRunning = false
    self.isPaused = true
    for k,node in pairs(self.nodes) do
        node:onGraphPaused()
    end
end

function BehaviourTree:load(filepath)
    local file = io.input(filepath)
    local jsonData = io.read("*a")
    local data = bt.decodeJson(jsonData)
    for k,v in pairs(data.nodes) do
        print(v["$id"])
    end
end